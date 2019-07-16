using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RabbitMQPlayground.Routing.Shared
{
    public class RabbitMQSubjectExpressionMember
    {
        public string Property { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }
    }

    public class RabbitMQSubjectExpressionVisitor : ExpressionVisitor
    {

        private readonly Type _declaringType;
        private readonly List<RabbitMQSubjectExpressionMember> _members = new List<RabbitMQSubjectExpressionMember>();
        private RabbitMQSubjectExpressionMember _current;
        private string _debug;
        private readonly List<string> _usedMembers = new List<string>();

        public RabbitMQSubjectExpressionVisitor(Type declaringType)
        {
            _declaringType = declaringType;
        }

        public string Resolve()
        {
            if (_members.Count == 0) return "#";

            var expectedRoutingAttributes = _declaringType.GetProperties().Where(property => property.IsDefined(typeof(RoutingPositionAttribute), false))
                                                                          .Count();
            var subject = string.Empty;

            void appendSubject(string str)
            {
                if (string.Empty == subject) subject += str;
                else
                {
                    subject += $".{str}";
                }
            }


            for (var i = 0; i < expectedRoutingAttributes; i++)
            {
                var member = _members.FirstOrDefault(m => m.Position == i);

                var value = (null == member) ? "*" : member.Value;

                appendSubject(value);
            }
                                

            return subject;
        }

        public override Expression Visit(Expression expr)
        {
            if (expr != null)
            {

                switch (expr.NodeType)
                {
                    case ExpressionType.MemberAccess:

                        var memberExpr = (MemberExpression)expr;
                        if (memberExpr.Member.DeclaringType.IsAssignableFrom(_declaringType))
                        {
                            var member = memberExpr.Member.Name;

                            if (!memberExpr.Member.CustomAttributes.Any(attr=> attr.AttributeType == typeof(RoutingPositionAttribute)))
                                throw new InvalidOperationException($"only properties decorated with the RoutingPosition attribute are allowed [{member}]");

                            if(_usedMembers.Contains(member)) throw new InvalidOperationException($"expression should only referenced memeber one time [{member}]");

                            _usedMembers.Add(member);

                            var attribute = memberExpr.Member.GetCustomAttributes(typeof(RoutingPositionAttribute), false).First() as RoutingPositionAttribute;

                            _current = new RabbitMQSubjectExpressionMember()
                            {
                                Position = attribute.Position,
                                Property = $"{member}"
                            };

                            _members.Add(_current);

                            _debug += $"{member} ";
                        }

                        break;

                    case ExpressionType.AndAlso:

                        _debug += "AndAlso ";

                        break;

                    case ExpressionType.Equal:
                        _debug += "Equal ";
                        break;

                    case ExpressionType.Constant:
                        var constExp = (ConstantExpression)expr;

                        if (null != _current)
                        {
                            _debug += $"{constExp.Value} ";

                            //todo handle only primitive types
                            _current.Value = $"{constExp.Value}";

                        }

                        break;

                    case ExpressionType.Lambda:

                        _members.Clear();
                        _current = null;

                        break;
                    case ExpressionType.Parameter:
                        break;

                    //we only AndAlso Member Equal type, anything else cannot be rendered as a RabbitMQ subject
                    default:
                        throw new InvalidOperationException($"invalid {expr.NodeType}");

                }

            }

            return base.Visit(expr);
        }

        public override string ToString()
        {
            return _debug;
        }
    }
}
